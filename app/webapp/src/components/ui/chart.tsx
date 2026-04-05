import React, { useMemo } from 'react';
import { Group } from '@visx/group';
import { AreaClosed } from '@visx/shape';
import { AxisBottom, AxisLeft } from '@visx/axis';
import { curveMonotoneX } from '@visx/curve';
import { scaleTime, scaleLinear } from '@visx/scale';
import { GridRows, GridColumns } from '@visx/grid';
import useMeasure from 'react-use-measure';
import { motion } from 'motion/react';

interface DataPoint {
  date: Date;
  value: number;
}

interface AreaChartProps {
  data: DataPoint[];
  height?: number;
}

export const AreaChart = ({ data, height = 300 }: AreaChartProps) => {
  const [ref, { width }] = useMeasure();

  const margin = { top: 20, right: 20, bottom: 40, left: 40 };
  const innerWidth = width - margin.left - margin.right;
  const innerHeight = height - margin.top - margin.bottom;

  const xScale = useMemo(
    () =>
      scaleTime({
        range: [0, innerWidth],
        domain: [Math.min(...data.map(d => d.date.getTime())), Math.max(...data.map(d => d.date.getTime()))],
      }),
    [innerWidth, data]
  );

  const yScale = useMemo(
    () =>
      scaleLinear({
        range: [innerHeight, 0],
        domain: [0, Math.max(...data.map(d => d.value)) * 1.2],
        nice: true,
      }),
    [innerHeight, data]
  );

  if (width === 0) return <div ref={ref} style={{ height }} />;

  return (
    <div ref={ref} className="w-full" style={{ height }}>
      <svg width={width} height={height}>
        <Group left={margin.left} top={margin.top}>
          <GridRows scale={yScale} width={innerWidth} stroke="var(--border)" strokeOpacity={0.2} pointerEvents="none" />
          <GridColumns scale={xScale} height={innerHeight} stroke="var(--border)" strokeOpacity={0.2} pointerEvents="none" />
          
          <motion.g
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8, ease: "easeOut" }}
          >
            <AreaClosed<DataPoint>
              data={data}
              x={d => xScale(d.date.getTime()) ?? 0}
              y={d => yScale(d.value) ?? 0}
              yScale={yScale}
              strokeWidth={2}
              stroke="var(--primary)"
              fill="url(#area-gradient)"
              curve={curveMonotoneX}
            />
          </motion.g>

          <linearGradient id="area-gradient" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--primary)" stopOpacity={0.3} />
            <stop offset="100%" stopColor="var(--primary)" stopOpacity={0} />
          </linearGradient>

          <AxisBottom
            top={innerHeight}
            scale={xScale}
            stroke="var(--border)"
            tickStroke="var(--border)"
            tickLabelProps={{
              fill: "var(--muted-foreground)",
              fontSize: 11,
              textAnchor: 'middle',
            }}
          />
          <AxisLeft
            scale={yScale}
            stroke="var(--border)"
            tickStroke="var(--border)"
            tickLabelProps={{
              fill: "var(--muted-foreground)",
              fontSize: 11,
              textAnchor: 'end',
              dx: -4,
            }}
          />
        </Group>
      </svg>
    </div>
  );
};
